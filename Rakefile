task :default => [:fmt, :test]

task :build do
  sh "dotnet build"
end

task :test do
  sh 'dotnet test --logger "console;verbosity=normal"'
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
    f = File.open("tests/ExpressionTests_gen.cs", "w")
    Dir.chdir "tmp/int_tests"

    types = [""] + TYPEMAP.values
    f.puts "#pragma warning disable format"
    f.puts "public partial class ExpressionTests {"
    2.times do |idx|
      ["4", "0", "-4"].each do |op2_orig|
        types.repeated_permutation(2).each do |type1, type2|
          next if type1 == ""

          op = "+"
          val1 = 123

          op2 = op2_orig
          op2 = "(#{type2})#{op2}" unless type2.empty?

          code =
            case idx
            when 0
              <<~CODE
                #{type1} x = #{val1};
                var y = x #{op} #{op2};
                Console.WriteLine($"{y.GetType()} {y}");
              CODE
            when 1
              <<~CODE
                #{type1} x = #{val1};
                var y = #{op2} #{op} x;
                Console.WriteLine($"{y.GetType()} {y}");
              CODE
            end

          File.write("Program.cs", code)
          result = `dotnet run -v q --no-restore 2> /dev/null`.strip
          if $?.success?
            a = result.split.map{ |x| TYPEMAP[x] || x }
            fun = "check#{idx}"
          else
            a = []
            fun = "check#{idx}_err"
          end

          a.append(type1, val1, op, op2)
          method_name = "check#{idx}_#{type1}_#{type2}_#{op2_orig.tr('-','m')}"
          line =
            if !fun['err']
              sprintf "    [Fact] void %-25s { %s(%-9s %-4s %-9s %-4s %s %-11s); }", "#{method_name}()", fun,
                a[0].inspect + ",",
                a[1] + ",",
                a[2].inspect + ",",
                a[3].inspect + ",",
                a[4].inspect + ",",
                a[5].inspect
            else
              sprintf "    [Fact] void %-25s { %s(%s, %s, %s, %s); }", "#{method_name}()", fun, *a.map(&:inspect)
            end
          f.puts line
          puts line
        end # types
      end # op2
    end # idx
    f.puts "}"
    f.puts "#pragma warning restore format"
    f.close
  end
end
