TRIK-Upload
==========

An extension for VS2012/VS2013/VS2015 for deploying .NET projects on controllers or remote linux computers.


The plugin was designed to work with controllers like Raspberry Pi and TRIK. But it requires only SSH-connection for uploading and communication (and mono for running uploaded programs), so you can use it with any other linux remote machine.


##Features
+ Incremental deployment of your local project build-directory to remote Raspberry Pi/TRIK controller.
+ Fast switching between different projects in your solution.
+ Ability to start and stop execution of uploaded application.
+ Interactive remote stdout and stderr streams in the Visual Studio's output pane.
+ Start script generation

##Description
Nowadays it's very popular to use modern languages and language platforms for robotics/embedded programming. Following that path allows you to adopt libraries and tools developed for the language itself (making embedded devices much more comfortable to work with). 

.NET framework has been ported to ARM long time ago and Mono runtime and JIT fits naturally on modern ARM controllers and SoCs. All of these makes .NET an interesting platform for robotics and IoT development. 
Moreover, the platform-independent CLR byte-code eliminates the neccessity of cross-compiling opening broader horizons for library and tools reuse. The main deployment process is truncated to direct "host to target" file transferring.

Modern controllers are equipped with Wi-Fi which allows us to use different file-transferring protocols for project uploading.
However, handy methods for working with remote computers are mostly exceptional functionality in a modern IDE than common practice. Which brings developers to constant switching between rich IDE to CLI ssh and scp (or WinSCP) tools for uploading and running apps.

It would be nice if an IDE hid the differences of local and remote development models and offered tools for working with remote applications the same way we do with the local ones. 

Reducing these differences is the main task for __TRIK-Upload__. It's fully integrated to VS infrastructure through standard toolbar and outputpane which delivers to you an interactive output from remote running program. 
__TRIK-Upload__ also has set of clumsy hotkeys for uploading/running/stopping/configuring your project (it's actually hard to find free consistent key combinations in a big IDE). 

## Notes
For installation, using, and futher documentation see [project-wiki](https://github.com/kashmervil/Upload-Extension/wiki)


The plugin is still in beta and needs features, email me for getting the roadmap.
Even though __TRIK-Upload__ gives me a huge productivity gain with my another project [trik-sharp](http://www.github.com/kashmervil/trik-sharp) which stands for .NET library for robotics programming. 


## Build ##

*Please note that You need to have VS2013 SDK to build the project* (in case You are trying to build this project in VS2013)

## Installing

__TRIK-Upload__ can be installed with VS > Tools > Extensions and updates > Online

