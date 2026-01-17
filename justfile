set dotenv-load

clean:
	rm -rf dist

pack: clean
	dotnet pack -c Release -o dist

publish: pack
	dotnet nuget push dist/*.nupkg -s https://api.nuget.org/v3/index.json -k "$NUGET_API_KEY" 

