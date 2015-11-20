Naggy is an Atmel Studio extension that uses the Clang frontend from the LLVM project to show errors/warnings on the fly, and to lowlight code excluded by preprocessor directives.

0.4.0
-----

* Upgrade to Oct version of LLVM/Clang, and use avr-llvm project instead of custom patch.
* Upgrade project to VS 2015, and build for Atmel Studio 7.0
* Make Naggy aware of the new pack based device support mechanism used by Atmel Studio 7.0
* Define __NAGGY__  as a preprocessor symbol to let code know it's being processed by Naggy.
* Fix broken preprocessor include directories processing - make Clang look at user specified directories first, and implicit compiler directories next.
* Misc bug fixes.


0.3.7
-----

* Fix broken mode attribute interpretation in AVR projects, which was causing spurious warnings for types in stdint.h (like uint32_t).

0.3.6
-----

* Make Naggy know about AVR and its type sizes.
* Make Naggy aware of toolchain type (ARM or AVR) and use the correct target triple.
* Prefix Naggy's diagnostics with a [N] to make it distinct from build errors.

0.3.5
------

* Fix wrong diagnostic for C++ destructors defined outside the class
* Fix broken C++ 11 support
* Don't explicitly show the Error window from Naggy.

0.3.4
------

* Add C++ and C++11 diagnostics support for C++ projects in Atmel Studio.
* Fix broken support for ARM projects (include path resolution and symbol definition). 

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
