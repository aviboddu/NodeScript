namespace NodeScript;

public class Chunk
{
    public List<Operation> operations = [];
    public List<object> constants = [];
    public List<(int line, int column)> locations = [];


    private string OperationToString(int idx)
    {
        return $"{operations[idx].opCode} {operations[idx].data} line {locations[idx].line} column {locations[idx].column}";
    }
}