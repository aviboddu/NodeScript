using static NodeScript.CompilerUtils;

namespace NodeScript;

/**
<summary> A set of nodes makes up a Script. This includes input and output nodes. </summary>
*/
public class Script
{
    private readonly List<NodeData> nodesData = [];
    private Node[] Nodes = [];
    private Node[] NodesToExecute = [];

    /// <summary>
    /// Callback for compilation errors. 
    /// </summary>
    public ErrorHandler? CompileError;

    /// <summary>
    /// Callback for runtime errors.
    /// </summary>
    public ErrorHandler? RuntimeError;

    /// <summary>
    /// Creates an empty script
    /// </summary>
    public Script() { }

    /// <summary>
    /// Creates an empty script
    /// </summary>
    /// <param name="compileError">Callback for compilation errors.</param>
    /// <param name="runtimeError">Callback for runtime errors.</param>
    public Script(ErrorHandler compileError, ErrorHandler runtimeError)
    {
        CompileError += compileError;
        RuntimeError += runtimeError;
    }

    /// <summary>
    /// Adds an input node to the script with the given <paramref name="inputData"/>. The input node will send one line of data at a time.
    /// </summary>
    /// <param name="inputData">A string containing all input data</param>
    /// <returns>A non-negative ID.</returns>
    /// <exception cref="ArgumentException">Thrown if the script already has an input node. Each script can only have one input node. </exception>
    public int AddInputNode(string inputData)
    {
        if (nodesData.Any(x => x is InputNodeData))
            throw new ArgumentException("Can only have one input node");
        nodesData.Add(new InputNodeData(inputData));
        return nodesData.Count - 1;
    }

    /// <summary>
    /// Adds an output node to the script. The output node will store all data sent to it.
    /// </summary>
    /// <returns>A non-negative ID.</returns>
    /// <exception cref="ArgumentException">Thrown if the script already has an output node. Each script can only have one output node. </exception>
    public int AddOutputNode()
    {
        if (nodesData.Any(x => x is OutputNodeData))
            throw new ArgumentException("Can only have one output node");
        nodesData.Add(new OutputNodeData());
        return nodesData.Count - 1;
    }

    /// <summary>
    /// Adds a combiner node to the script. A combiner node can receive multiple inputs and send a single output.
    /// </summary>
    /// <returns>A non-negative ID.</returns>
    public int AddCombinerNode()
    {
        nodesData.Add(new CombinerNodeData());
        return nodesData.Count - 1;
    }

    /// <summary>
    /// Adds a regular node to the script. A regular node will execute the given <paramref name="code"/> line-by-line, receiving and sending output accordingly.
    /// </summary>
    /// <param name="code">The code to compile and run.</param>
    /// <returns>A non-negative ID.</returns>
    public int AddRegularNode(string code)
    {
        nodesData.Add(new RegularNodeData(code));
        return nodesData.Count - 1;
    }

    /// <summary>
    /// Connects two nodes in a script. The first node sends its output to the second node.
    /// </summary>
    /// <param name="nodeId">The first node to connect</param>
    /// <param name="outputId">The second node to connect</param>
    /// <exception cref="ArgumentException">Thrown if the connection is invalid, such as trying to connect a node to itself or trying to output to an input node</exception>
    public void ConnectNodes(int nodeId, int outputId)
    {
        ValidateIds(nodeId, outputId);
        if (nodeId == outputId) throw new ArgumentException("Ids cannot be equal");

        NodeData outputNodeData = nodesData[outputId];
        if (outputNodeData is InputNodeData)
            throw new ArgumentException("Cannot output to an input node");

        NodeData nodeData = nodesData[nodeId];
        switch (nodeData)
        {
            case OutputNodeData:
                throw new ArgumentException("Output Node doesn't have its own output.");
            case CombinerNodeData:
            case InputNodeData:
                if (nodeData.Outputs is not null) throw new ArgumentException("This node can only have one output");
                nodeData.Outputs = [outputId];
                break;
            case RegularNodeData:
                nodeData.Outputs ??= [];
                nodeData.Outputs = [.. nodeData.Outputs, outputId];
                break;
        }
    }

    /// <summary>
    /// Updates the data for a particular node. This node can either be an input node (updating the input data) or a regular node (updating the code)
    /// </summary>
    /// <param name="id">The id of the node to be updated</param>
    /// <param name="newData">Either the new input data or the new code</param>
    /// <exception cref="ArgumentException">Thrown if the identified node is not an input or regular node, or if the <paramref name="id"/> is invalid</exception>
    public void UpdateData(int id, string newData)
    {
        ValidateIds(id);
        NodeData nodeData = nodesData[id];
        switch (nodeData)
        {
            case RegularNodeData r:
                r.Code = newData;
                break;
            case InputNodeData i:
                i.InputData = newData;
                break;
            default:
                throw new ArgumentException("Node must be an input node or regular node");
        }
    }

    /// <summary>
    /// Attempts to compile all nodes.
    /// </summary>
    /// <returns><c>true</c> if compilation was successful or <c>false</c> otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no input node.</exception> 
    public bool CompileNodes()
    {
        Nodes = new Node[nodesData.Count];
        for (int i = 0; i < nodesData.Count; i++)
            if (!CompileNode(i)) return false;

        for (int i = 0; i < nodesData.Count; i++)
            LinkNode(i);

        Queue<Node> nodeQueue = new([Nodes.OfType<InputNode>().Single()]);
        List<Node> executionList = new(Nodes.Length);
        while (nodeQueue.Count > 0)
        {
            Node node = nodeQueue.Dequeue();
            executionList.Add(node);
            foreach (Node output in node.OutputNodes())
            {
                if (!executionList.Contains(output))
                    nodeQueue.Enqueue(output);
            }
        }
        NodesToExecute = [.. executionList];

        return true;
    }

    /// <summary>
    /// Resets all nodes to their initial state.
    /// </summary>
    public void Reset()
    {
        foreach (Node node in NodesToExecute)
            node.Reset();
    }

    /// <summary>
    /// Runs the script in its entirety.
    /// </summary>
    public void Run()
    {
        do
        {
            StepLine();
        } while (NodesToExecute.Any(n => n.State == NodeState.RUNNING));
    }

    /// <summary>
    /// Runs the script for a single cycle.
    /// </summary>
    public void StepLine()
    {
        foreach (Node node in NodesToExecute)
            node.StepLine();
    }

    /// <summary>
    /// Provides the current output in the output node
    /// </summary>
    /// <returns>All the output data, or <c>null</c> if there is no output node.</returns>
    public string? GetOutput()
    {
        OutputNode? outputNode = Nodes.OfType<OutputNode>().SingleOrDefault();
        return outputNode?.Output;
    }

    /// <summary>
    /// Gets the current line of the identified regular node.
    /// </summary>
    /// <param name="node_id">The ID of the node.</param>
    /// <returns>The current line of the identified node.</returns>
    /// <exception cref="ArgumentException">Thrown if the identified node is not a regular node.</exception>
    public int GetCurrentLine(int node_id)
    {
        ValidateIds(node_id);
        if (Nodes[node_id] is RegularNode r)
            return r.GetCurrentLine();
        throw new ArgumentException("Node must be a regular node.");
    }

    private bool CompileNode(int id)
    {
        NodeData nodeData = nodesData[id];
        switch (nodeData)
        {
            case CombinerNodeData:
                Nodes[id] = NodeFactory.CreateCombinerNode();
                break;
            case OutputNodeData:
                Nodes[id] = NodeFactory.CreateOutputNode();
                break;
            case InputNodeData i:
                Nodes[id] = NodeFactory.CreateInputNode(i.InputData);
                break;
            case RegularNodeData r:
                RegularNode? regularNode = NodeFactory.CreateRegularNode(r.Code, BindErrorHandler(CompileError, id), BindErrorHandler(RuntimeError, id));
                if (regularNode is null) return false;
                Nodes[id] = regularNode;
                break;
        }
        return true;
    }

    private void LinkNode(int id)
    {
        Node node = Nodes[id];
        NodeData nodeData = nodesData[id];
        Node[]? outputs = nodeData.Outputs?.Select(i => Nodes[i]).ToArray();
        switch (node)
        {
            case CombinerNode c:
                c.output = outputs?[0];
                break;
            case InputNode i:
                i.output = outputs?[0];
                break;
            case RegularNode r:
                r.outputs = outputs;
                break;
        }
    }

    private void ValidateIds(params int[] ids)
    {
        foreach (int id in ids)
            if (id < 0 || id > nodesData.Count) throw new ArgumentException($"id {id} is out of range");
    }

    private abstract class NodeData(int[]? Outputs = null)
    {
        public int[]? Outputs = Outputs;
    }

    private class InputNodeData(string InputData) : NodeData
    {
        public string InputData = InputData;
    }

    private class OutputNodeData() : NodeData;
    private class CombinerNodeData() : NodeData;

    private class RegularNodeData(string Code) : NodeData
    {
        public string Code = Code;
    }

    private static InternalErrorHandler BindErrorHandler(ErrorHandler? handler, int id)
    {
        return (int line, string message) => handler?.Invoke(id, line, message);
    }
}

public delegate void ErrorHandler(int node_id, int line, string message);