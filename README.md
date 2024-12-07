# TACBuilder 

## Setup 

1. Create a publication: `dotnet publish -c Release --self-contained`
2. Build a TACBuilder: `dotnet build -c Debug`

## Run (Console) 

Use `./TACBuilder -m console -f <asm1.dll> <asm2.dll>` where `<asmX.dll>` is a absolute path to dll from publication
