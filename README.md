# TACBuilder 

## Setup 

1. Create a publication for a project you want to explore: `dotnet publish -c Release --self-contained`
2. Build a TACBuilder: `dotnet build -c Debug`

## Run (Console) 

Use `./TACBuilder -m console -f <asm1.dll> <asm2.dll>` where `<asmX.dll>` is a absolute path to dll from publication


### Instance naming 

#### Type 

There are multiple situations, from simple to complex: 

1. Non-generic, not nested: `namespace.typeName`
2. Non-generic, nested: `namespace.declType+typeName`
3. Generic type parameter, not nested: `namespace.declaringType!typeName`
4. Generic type parameter, nested: `namespace.declaringType+declarintType!typeName`
5. Generic method parameter: `methodNonGenericSignature!typeName`

#### Method

There are 2 situations: 

1. Non-generic: `returnType declType.methodName(param1,param2,...)`
2. Generic: `returnType declType.methodName<genericArg1,genericArg2,...>(param1,param2,...)`

#### Notes

1. Note that typeName and methodName should contain number of generic args, e.g. List<T> -> List`1
2. methodName initially does not have \`n where n is number of generic parameters whilst it is essential 

#### PoC


