# Working with Hair
---- working notes
Something about using Grooms. Good resource for more words https://github.com/Unity-Technologies/com.unity.demoteam.hair 
Generated from Alembic files with curves
---/notes

when importing an alembic file, Unity can be used with the [Hair package](link) to generate a [groom](link) based on an Alembic files containing curves.

To enable this workflow, you must have the Hair package installed and your alembic must contain curves or curve data. 
Make sure that the Import Curves setting is enabled.
 
## Generating a Hair Asset

1. In the Project view, select the Alembic file.

2. In the Inspector, Select the  Model tab.

3. Verify that the Import Curves setting is enabled.

4. In the Inspector, select the Hair tab.

5. Click the Generate Hair Asset button to generate a new Hair Asset based on the Alembic file.

The Hair Asset file will be generated in the same project folder as the Alembic that it is based on.

Now you can harness the all the capabilities of the Hair package!