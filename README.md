# TinyProc
A virtual CPU emulator with a full featured custom architecture and instruction set.

## Project status: <span style="color: lime;">Active</span>

## What is this about?
This project is an attempt at learning how CPUs work by implementing one from scratch.
Be reminded though that there is no focus on making things clean and readable, so don't
expect this to be easily understandable. Although if you want to continue reading,
there are some useful references and documentation in the `Microarchitecture/` directory.<br>
Note that everything is WIP and in early development.
If you want to test the latest stable version, try the `main` branch or the latest release.
The most recent code is in the `development` branch. It is not guaranteed this code won't even run
at all. For long-term changes, additional branches might be added temporarily.

## Building & Running
### Prerequisites and Dependencies
Supported platforms:
- Linux x64 (kernel 6.17.0 or later)
- Windows x64 (10 or later)
- Android aarch64 (via Termux, without GUI)

Required dependencies:
- [.NET](https://dotnet.microsoft.com/en-us/download) (9.0 or later, mandatory)
- [GNU Make](https://www.gnu.org/software/make/) (4.4.1 or later)

*The specified versions are verified to be working, but not strictly required, except those marked as "mandatory"*

### Running the GUI
Starting the GUI (graphical user interface) is done simply by running
```
$ make gui
```
*Documentation on what every GUI element does will be added later, although most of it should be self-explanatory.*

### Running assembly files (CLI mode)
**Please note that the current processor revision is in early development and subject to drastic changes potentially breaking compatibility.**<br>
**It is recommended you familiarize with the HLTP32 ISA before writing custom code. Documentation is available in the `Microarchitecture/` directory**

First, find or create an assembly source file to run and save it.<br>
Demos / Example files can be found in the `prog_examples/` directory.<br>
To run an assembly file from the project root folder, run
```
$ make emu-run SOURCE_FILE_ASM=<path>
```
where `<path>` is the source assembly file.

To just assemble the file without running it, run
```
$ make assemble SOURCE_FILE_ASM=<path>
```

### Creating a standalone binary
Since .NET supports AOT-compilation, you may create a native executable binary using
```
$ make emu-build
```
or
```
$ make gui-build
```
To run these AOT-compiled executables directly from Make, run
```
$ make emu-run-aot
```
or
```
$ make gui-run-aot
```
respectively.

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
- Hardware interrupts, MMIO
- video buffers (video memory & console mode)
- Faster emulator in C (C# <-> C via FFIs)
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

Depending on motivation and how busy I am otherwise, this project might have
phases of several months without updates. I am, however, committed to keep the project running
and will keep the project status display above up to date accordingly.
You can see if the project is receiving updates at the very top of the README.

## Contributing
I am currently not accepting contributions, but feel free to fork the project and make your own changes.