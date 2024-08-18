## Check

- every instruction producing tac line use `PopSingleAddr()` instead of `Pop()`
- ldvirtftn seem like pushing ptr onto stack, now creates tac that load the method

## Test

- arglist
- fault block

## Decide

- ptr / ref need target type
- ptr / deref ops are diffrent for managed / unmanaged
- native int implements unmanaged ptr
- handling instance..ctor as another stmt / expr
- introduce special instances for raw mem access (initobj handling for example)
- defaul ctor, ctor call, other instance init methods separation