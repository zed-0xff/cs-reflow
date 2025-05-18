task :default => [:fmt, :test]

task :build do
  sh "dotnet build"
end

task :test do
  sh "dotnet test"
end

task :fmt do
  sh "dotnet format reflow.sln"
end
