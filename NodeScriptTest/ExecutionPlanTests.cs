namespace NodeScriptTest;

using NodeScript;

[TestClass]
public class ExecutionPlanTests
{
    [TestMethod]
    public void RejectsBranchTargetOutsideInstructionBoundary()
    {
        Compiler.CompiledData data = new([(byte)OpCode.JUMP, 1, 0, (byte)OpCode.RETURN], [], [0], 2, 0);

        Assert.IsFalse(ExecutionPlan.TryCreate(data, out _, out string? error));
        StringAssert.Contains(error, "branch target");
    }

    [TestMethod]
    public void RejectsStackUnderflow()
    {
        Compiler.CompiledData data = new([(byte)OpCode.POP, (byte)OpCode.RETURN], [], [0], 2, 0);

        Assert.IsFalse(ExecutionPlan.TryCreate(data, out _, out string? error));
        StringAssert.Contains(error, "Stack underflow");
    }

    [TestMethod]
    public void RejectsKnownTypeMismatch()
    {
        Compiler.CompiledData data = new([(byte)OpCode.TRUE, (byte)OpCode.TRUE, (byte)OpCode.ADDI, (byte)OpCode.RETURN], [], [0], 2, 0);

        Assert.IsFalse(ExecutionPlan.TryCreate(data, out _, out string? error));
        StringAssert.Contains(error, "Invalid stack type");
    }

    [TestMethod]
    public void RejectsMergedPathsWithDifferentStackHeights()
    {
        Compiler.CompiledData data = new(
            [(byte)OpCode.TRUE, (byte)OpCode.JUMP_IF_FALSE, 4, 0, (byte)OpCode.TRUE, (byte)OpCode.JUMP, 0, 0, (byte)OpCode.RETURN],
            [],
            [0],
            2,
            0);

        Assert.IsFalse(ExecutionPlan.TryCreate(data, out _, out string? error));
        StringAssert.Contains(error, "Incompatible stack heights");
    }

    [TestMethod]
    public void AcceptsMergedPathsWithMatchingStackState()
    {
        Compiler.CompiledData data = new(
            [
                (byte)OpCode.TRUE,
                (byte)OpCode.JUMP_IF_FALSE, 4, 0,
                (byte)OpCode.TRUE,
                (byte)OpCode.JUMP, 1, 0,
                (byte)OpCode.FALSE,
                (byte)OpCode.POP,
                (byte)OpCode.RETURN,
            ],
            [],
            [0],
            2,
            1);

        Assert.IsTrue(ExecutionPlan.TryCreate(data, out _, out string? error), error);
    }
}
