using static NodeScript.CompilerUtils;

namespace NodeScript;

public class Script
{
    private readonly List<NodeData> nodesData = [];
    public Node[] Nodes { get; private set; } = [];
    private Node[] NodesToExecute = [];

    public ErrorHandler? CompileError;
    public ErrorHandler? RuntimeError;

    public Script() { }
    public Script(ErrorHandler compileError, ErrorHandler runtimeError)
    {
        CompileError += compileError;
        RuntimeError += runtimeError;
    }

    public int AddInputNode(string inputData)
    {
        if (nodesData.Any(x => x is InputNodeData))
            throw new ArgumentException("Can only have one input node");
        nodesData.Add(new InputNodeData(inputData));
        return nodesData.Count - 1;
    }

    public int AddOutputNode()
    {
        if (nodesData.Any(x => x is OutputNodeData))
            throw new ArgumentException("Can only have one output node");
        nodesData.Add(new OutputNodeData());
        return nodesData.Count - 1;
    }

    public int AddCombinerNode()
    {
        nodesData.Add(new CombinerNodeData());
        return nodesData.Count - 1;
    }

    public int AddRegularNode(string code)
    {
        nodesData.Add(new RegularNodeData(code));
        return nodesData.Count - 1;
    }

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

    public void Reset()
    {
        foreach (Node node in NodesToExecute)
            node.Reset();
    }

    public void Run()
    {
        do
        {
            StepLine();
        } while (NodesToExecute.Any(n => n.State == NodeState.RUNNING));
    }

    public void StepLine()
    {
        foreach (Node node in NodesToExecute)
            node.StepLine();
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