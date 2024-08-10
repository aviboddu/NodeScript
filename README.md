# NodeScript
NodeScript is a rudimentary programming language designed to function on 'nodes', intended for use in puzzle games. Find development details [here](./Development.md)

## Nodes
- Input nodes will continuously attempt to send the next line. It has only one output.
- Regular nodes can take in one string at a time, process it and send it to a set number of outputs. Each node can store a single object in `mem` and has limited space for code.
- Combiner nodes can merge 2 pipes into one. It offers no options to choose which string goes through first, picking whatever comes first.
- Output nodes will consume the lines sent to it, placing it in the output file.

## Features
- Basic arithmetic and boolean logic
- Variables - All variables are global in scope
- Indexing
    - Element_of: `v[i]`
    - Slice: `v[i:j]`
- Basic control flow (if-else)
- Native functions for things like:
    - String manipulation
    - Data conversion and parsing
    - Indexing and slicing
- Dynamic typing between:
    - string
    - string[]
    - int
    - bool

Notably, there are **NO** loops within the scripting itself. No while. No for.
Execution will occur line by line and will only start when a node receives an input string to be processed.

## Syntax
Every line contains a single statement. All statements will start with a relevant keyword for the operation and end with a semicolon.
- SET: Sets a variable to a certain value. Variables do not need to be declared. Syntax: `SET <variable_name>, <expression>;`
- PRINT: Sends a string to a specific output node, denoted by an index. Syntax: `PRINT <output_idx>, <expression>;`
- RETURN: Ends the program (until the next input comes). Syntax: `RETURN;`
- IF: Executes the following code if the given expression is true. Syntax `IF <expression>;`
- ELSE: Executes the following code if the previous if statement was false. Syntax `ELSE;`
- ENDIF: Marks the end of the IF clause. Either ends the IF code section or the ELSE code section. Only one is needed per IF/ELSE statement. Syntax `ENDIF;`

There are no methods nor is there indexing. Several native functions are available to provide necessary functionality.