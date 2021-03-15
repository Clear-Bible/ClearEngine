# ClearEngine3

DotNet library for the Clear technology 
to aid in Bible translation as developed by Clear Bible, Inc.

(Currently under development and should be considered a prototype.)

# Getting Started

To run the console application in RegressionTest1, first adjust
the startup folder to be:
  (repository)/test/TestSandbox1

For RegressionTest3, adjust the startup folder to be:
  (repository)/test/RegressionTest3

If you are using Visual Studio Community on MacOSX, the startup folder for a project may be adjusted by:

 - highlighting the project in the solution explorer,
 - selecting Options from the context menu,
 - selecting Run | Configurations | Default in the project options dialog,
 - adjusting the "Run in directory" field, perhaps by browsing to the desired location using the "..." button to the right.

If you are using Visual Studio Community on Windows, the startup folder may be adjusted by:

- highlighting the project in the solution explorer,
- selecting Properties from the context menu,
- in the editor that opens, choosing the Debug tab in the list at the left,
- adjusting the Working Directory field, perhaps by using the Browse button to the right.

The regression tests may be further configured by editing the source code at the beginning of the tests.

The tests produce output in the form of an alignment.json file.  In the folders where these output files are created, you will also find files with names like alignment.json.cmp2 to which the output may be compared.  You should expect the output to match the comparison file with the highest numbered final digit.
