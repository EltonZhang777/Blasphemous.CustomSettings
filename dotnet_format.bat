dotnet restore
dotnet format whitespace --no-restore --exclude Blasphemous.CustomSettings\obj
dotnet format style --no-restore --severity info --exclude Blasphemous.CustomSettings\obj