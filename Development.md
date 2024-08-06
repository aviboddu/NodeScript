# Development
The NodeScript folder contains all required code for the library. The NodeScriptTest folder is for, well, tests. NodeScript is compiled in the following steps:
- Tokenization: This converts the raw texts into a series of tokens, like `LEFT_PAREN` or `COMMA`. While more tokens are a single character, this section is also responsible for tokenizing literals and identifiers as well as skipping past comments.
- Parsing: This parses the stream of symbols into Operations, which represent the various operations one can do, such as printing or setting. The operations take on a tree structure, containing the relevant expressions which may themselves contain more expressions. The Parser can also perform some basic compile-time validation.
- Compiling: This step converts the stream of operations into raw bytecode, for rapid execution. This step is also responsible for setting up the jump statements as well as creating the constants table.
- Node: The node actually executes the code, using a stack and hash table for storage during the execution.

## Native Functions
Native Functions are all defined with a particular delegate in [NativeFuncs.cs](NodeScript/NativeFuncs/NativeFunc.cs). So long as the function signature is correct, all one has to do is create a new static method in the NativeFuncs class and it will be added to the language.

## Error Handling
Compilation errors and runtime errors separated into different handlers. One can pass delegates of the appropriate type in order to handle the errors. All errors halt the program/compilation.

## References
Many of the techniques used in the code base were learnt from the textbook [Crafting Interpreters](https://craftinginterpreters.com/)