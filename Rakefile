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

TYPEMAP = {
  "System.Byte" => "byte",
  "System.SByte" => "sbyte",
  "System.Int16" => "short",
  "System.UInt16" => "ushort",
  "System.Int32" => "int",
  "System.UInt32" => "uint",
  "System.Int64" => "long",
  "System.UInt64" => "ulong",
}

namespace :gen do
  desc "generate int tests"
  task :int_tests do
    FileUtils.mkdir_p "tmp"
    system "dotnet new console -o tmp/int_tests --force -v q"
    Dir.chdir "tmp/int_tests"

    types = [""] + TYPEMAP.values
    checks = []
    types.repeated_permutation(2).each do |type1, type2|
      next if type1 == ""

      op = "+"
      val1 = 123

      op2 = "4"
      op2 = "(#{type2})4" unless type2.empty?
      #printf "[.] %6s %s %9s = ", type1, op, op2
      #$stdout.flush

      code = <<~CODE
        #{type1} x = #{val1};
        var y = x #{op} #{op2};
        Console.WriteLine($"{y.GetType()} {y}");
      CODE

      File.write("Program.cs", code)
      result = `dotnet run -v q --no-restore 2> /dev/null`.strip
      if $?.success?
        a = result.split.map{ |x| TYPEMAP[x] || x }
        #puts a.join(" ")

        fun = "check"
      else
        #puts "Error"
        fun = "check_err"
        a = []
      end

      a.append(type1, val1, op, op2)
      puts "    [Fact] void check_#{type1}_#{type2}() { #{fun}(#{a.map(&:inspect).join(", ")}); }"
    end
  end
end
