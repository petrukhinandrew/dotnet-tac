## Check

- every instruction producing tac line use `PopSingleAddr()` instead of `Pop()`

## Test

- arglist
- fault block

## Decide

- ptr / ref need target type
- ptr / deref ops are diffrent for managed / unmanaged
- native int implements unmanaged ptr