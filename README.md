# C# Reflow
Restore obfuscated/flattened C# control flow.

See [tests/data/](tests/data/) for examples.

## Usage

```
  reflow [<filename> [<methods>...]] [options]

Arguments:
  <filename>  Input .cs file (optional).
  <methods>   Method names to process.

Options:
  --hint <hint>            Set lineno:bool control flow hint.
  -v, --verbose <verbose>  Increase verbosity level. [default: 0]
  -a, --all                Process all methods (default). [default: True]
  -T, --tree               Print syntax tree. [default: False]
  -c, --comments           Add comments. [default: True]
  --remove-switch-vars     Remove switch variables. [default: True]
  -P, --post-process       Post-process the code. [default: True]
  -q, --quiet              Suppress all debug/status output. [default: False]
  -l, --list               List methods. [default: False]
  --version                Show version information
  -?, -h, --help           Show help and usage information
```

## License

MIT
