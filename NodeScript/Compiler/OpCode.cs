namespace NodeScript;

internal enum OpCode : byte
{
    CONSTANT, // Push a constant to the stack
    TRUE, // Push true to the stack
    FALSE, // Push false to the stack
    POP, // Pop an element from the stack
    GET, // Get the value of a variable and push it to the stack
    SET, // Pop a value from the stack and set the variable to that value
    EQUAL, // Pop two values from the stack and check if they're equal
    NOT_EQUAL, // Pop two values from the stack and check if they're not equal
    GREATER, // Pop two values from the stack and check if the second is greater than the first
    GREATERI, // Pop two known integers from the stack and check if the second is greater than the first
    GREATER_EQUAL, // Pop two values from the stack and check if the second is greater than or equal to the first
    GREATER_EQUALI, // Pop two known integers from the stack and check if the second is greater than or equal to the first
    LESS, // Pop two values from the stack and check if the second is less than to the first
    LESSI,  // Pop two known integers from the stack and check if the second is less than the first
    LESS_EQUAL, // Pop two values from the stack and check if the second is less than or equal to the first
    LESS_EQUALI, // Pop two known integers from the stack and check if the second is less than or equal to the first
    ADD, // Pop two values from the stack and push their sum / concatenation
    ADDI, // Pop two known integers from the stack and push their sum
    ADDS, // Pop two known strings from the stack and push their concatenation
    ADDA, // Pop two known string arrays from the stack and push their concatenation
    SUBTRACT, // Pop two values from the stack and push value two minus value one
    SUBTRACTI, // Pop two known integers from the stack and push integer two minus integer one
    MULTIPLY, // Pop two values from the stack and push their product
    MULTIPLYI, // Pop two known integers from the stack and push their product
    DIVIDE, // Pop two values from the stack and push value two divided by value one
    DIVIDEI, // Pop two known integers from the stack and push integer two divided by integer one
    AND, // Pop two values from the stack and push value one AND value two
    ANDB, // Pop two known booleans from the stack and push boolean one AND boolean two
    OR, // Pop two values from the stack and push value one OR value two
    ORB, // Pop two known booleans from the stack and push boolean one OR boolean two
    NEGATE, // Pop a value from the stack and push its negation
    NEGATEI, // Pop a known integer from the stack and push its negation
    NOT, // Pop a value from the stack and push NOT value
    NOTB, // Pop a known boolean from the stack and push NOT boolean
    PRINT, // Pop an integer and string from the stack and try to push the string to output[integer]
    PRINTIS, // Pop a known integer and string from the stack and try to push the string to output[integer]
    JUMP, // Add the given offset to the instruction index
    JUMP_IF_FALSE, // Pop a value from the stack and add the given offset to the instruction index if the value is false
    CALL, // Pop one or more values off the stack and push the result of the corresponding native function call
    CALL_TYPE_KNOWN, // Pop one or more values off the stack and push the result of the corresponding native function call with known types
    RETURN, // Become idle
    ENDIF, // End the if block
    NOP, // Do nothing
}