# LunaticAstroEQ
Starting again, basic ASCOM LocalServer for AstroEQ controllers

LunaticAstroEQ.sln is a Visual Studio solution that includes projects for an ASCOM Telescope Driver and a Telescope Control client
for use with the ASCOM driver. The driver uses the LocalServer model.

All projects shoudld target .NET Framewwork 4.5. For the time being NO NOT update the nugets for the System.Reactive extensions beyond v4.1.2, MvvmLighLibs beyond v5.4.1.1 or CommonServiceLocator beyond v2.0.4. The latest version of everything (at the time of writing) will not work together. This may change in the future.

The telescope driver is being tested against Tom Carpenter's AstroEQ controller for stepper motor driven equatorial mounts. For more
details see Tom's web page at: https://www.astroeq.co.uk.

The Astro EQ controller uses the Skywatcher protocol so in theory this driver could work modified Skywatcher mounts in the same
way that EQMOD does but it has NOT been tested against these mounts.

For details of the EQMOD project see: http://eq-mod.sourceforge.net/

The software and source code is distributed under the BSD License 2.0.
