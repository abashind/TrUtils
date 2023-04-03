To use TrUtils, you need to set up your Testrail instance.
Add the next custom fields:
???
Add the next test statuses:
???
Your exception message should have the next structure to allow the analyzer to works as supposed:
???
You can store bug links either in the case entity, field - '' or in the result entity, field - ''. 
The 'Utility_0_AssignBugStatusInLastRun' util will find them, check if the ticket is still oped and them transfer the 'Bug' status to the test in the last run.