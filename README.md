# LunaticAstroEQ
Starting again, basic ASCOM LocalServer for AstroEQ controllers

LunaticAstroEQ.sln is a Visual Studio solution that includes projects for an ASCOM Telescope Driver and a Telescope Control client
for use with the ASCOM driver. The driver uses the LocalServer model.

The telescope driver is being tested against Tom Carpenter's AstroEQ controller for stepper motor driven equatorial mounts. For more
details see Tom's web page at: https://www.astroeq.co.uk.

The Astro EQ controller uses the Skywatcher protocol so in theory this driver could work modified Skywatcher mounts in the same
way that EQMOD does but it has NOT been tested against these mounts.

For details of the EQMOD project see: http://eq-mod.sourceforge.net/

The software and source code is distributed under the BSD License 2.0.
