Naggy is an Atmel Studio extension that uses the Clang frontend from the LLVM project to show errors/warnings on the fly, and to lowlight code excluded by preprocessor directives.

0.3.3
------

* Diagnostics parsing is disabled for header files.
* A new menu option (Tools -> Disable Naggy) can be used to turn Naggy on/off with immediate effect.
* Upgraded LLVM/Clang version used to 3.3

0.3.2
------

* C99 features now don't result in diagnostics, if the Atmel Studio project's command line specifies that the language standard is C99 (std=c99 or std=gnu99)
* Opening toolchain header files in the editor now works without spewing a bunch of diagnostics about missing includes.

0.3.1
------

* Diagnostics now show up in the ErrorList as well, in addition to editor squiggles.
* Diagnostics can now be blacklisted, and blacklisted diagnostics are not reported.
  Naggy ships with a few common false diags disabled, but the list is user 
  configurable (see https://github.com/saaadhu/naggy/wiki/FAQ).
* Miscellaneous bug fixes

0.3.0
------

* Upgraded clang/llvm to the latest SVN trunk (3.2+)
* Added Atmel Studio 6.1 to the list of supported products
* Added support for newly added devices
