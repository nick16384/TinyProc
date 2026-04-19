# TinyProc
A virtual CPU emulator with a full featured custom architecture and instruction set.

## Project status: <span style="color: lime;">Active</span>

## What is this about?
This project is an attempt at learning how CPUs work by implementing one from scratch, for
my own entertainment and as a learning tool.<br>
Documentation for the CPU Microarchitecture is available in the `Microarchitecture/` directory.<br>
Note that everything is WIP and in early development.
If you want to test the latest stable version, try using the latest release.
The most recent code is in the `development` branch. It is not guaranteed this code will even run
at all. For long-term changes, additional branches might be added temporarily.

## Building & Running
### Prerequisites and Dependencies
Supported platforms:
- Linux x64 (kernel 6.17.0 or later)
- Windows x64 (10 or later)
- Android aarch64 (via Termux, without GUI)

Required dependencies:
- [.NET](https://dotnet.microsoft.com/en-us/download) (9.0 or later, mandatory)
- [Python](https://www.python.org/downloads/) (3.13.7 or later)

*The specified versions are verified to be working, but not strictly required, except those marked as "mandatory"*

### Running the GUI
Starting the GUI (graphical user interface) is done simply by running
```
$ python3 ./tp_gui.py
```
*Documentation on what every GUI element does will be added later, although most of it should be self-explanatory.*

### Running assembly files (CLI mode)
**Please note that the current processor revision is in early development and subject to drastic changes potentially breaking compatibility.**<br>
**It is recommended you familiarize with the HLTP32 ISA before writing custom code. Documentation is available in the `Microarchitecture/` directory**

First, find or create an assembly source file to run and save it.<br>
Demos / Example files can be found in the `prog_examples/` directory.<br>
To run an assembly file from the project root folder, run
```
$ python3 ./tp_run.py <ASM_FILE_PATH>
```
where `<ASM_FILE_PATH>` is the source assembly file's path.

To just assemble the file without running it, run
```
$ python3 ./tp_asm.py <ASM_FILE_PATH>
```

### Creating a standalone binary
.NET theoretically supports AOT compilation to create native executables.
However, AOT compilation targets have yet to be implemented.

### Performance notes
The main emulator is **not** optimized for performance. Instead, it should be as close to the
actual hardware as possible, while still retaining object orientation.
In the future, a more performant variant in C might be implemented, which focuses on performance
and interacts with the main frameworks via C#'s FFI.
Note the AOT-compiled version might be a little faster than the JIT-compiled one.

## Roadmap (as of 15. Apr. 2026)
### Upcoming
- **GUI**
- **complete arch spec + fully implemented software ISA v1**
- **FPGA / Verilog implementation** (by [@Maxi12045](https://github.com/Maxi12045))
### Future ideas
- FFI abstraction -> emulators in other languages (C for performance, Python for readability)
- Hardware interrupts, MMIO
- video buffers (video memory & console mode)
- Brainfuck interpreter / compiler
- [BASIC](https://en.wikipedia.org/wiki/BASIC) implementation (programming language)
- 6502 / 8086 / x86 -> HLTP32 transpiler (simple instructions only)
- C compiler
- 2D rendering engine (flappy bird?)
- oscilloscope
- Doom

*Also see the `ToDo.txt` file for more detailed current and future work*

## Misc. Notes
Temporary project names and acronyms:<br>
*These names are subject to change and only act as placeholders until something more satisfying is found.*<br>
HLTPEmul: **H**ardware **L**evel **T**iny **P**rocessor **Emul**ator<br>
Internal architecture names: TinyProc/x25_32, HLTPx25_32, x25-32, HLTP32

The "Can it run Doom" project will be a big one. This will probably take years until
implemented, but it's seen as the ultimate long-term goal for it so I promise it **will** run
at some point in the future.

All of the code is guaranteed to not be AI slop (i.e. I have written every line by myself),
but I can't guarantee some degree of AI use to ask for abstract concepts, pseudocode
implementations and library specific functions.
Furthermore, I cannot guarantee the code to be free of slurs, although I avoid them whenever
possible.

Depending on motivation and how busy I am otherwise, this project might have
phases of several months without updates. I am, however, committed to keep the project running
and will keep the project status display above up to date accordingly.
You can see if the project is receiving updates at the very top of the README.

## Contributing
I am currently not accepting contributions, but feel free to fork the project and make your own changes.