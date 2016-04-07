# Elmah.Gelf #

Elmah module for sending alert messages to GrayLog server in Gelf format via UDP


### Getting Started ###

Download nuget package Elmah.Gelf

Configure it
```
#!xml

<configuration>
 ...
 <configSections>
  <sectionGroup name="elmah">   
    <section name="errorGelf" requirePermission="false" type="Elmah.Gelf.ErrorGelfSectionHandler, Elmah.Gelf" />
  </sectionGroup>
 </configSections> 
 ...
 <system.web>
  <httpModules>
   <add name="ErrorLog" type="Elmah.ErrorLogModule, Elmah" />
   <add name="ErrorGelf" type="Elmah.Gelf.ErrorGelfModule, Elmah.Gelf" />
  </httpModules>
 </system.web>
 ...
 <system.webServer>
  <modules>
   <add name="ErrorLog" type="Elmah.ErrorLogModule, Elmah" preCondition="managedHandler" />
   <add name="ErrorGelf" type="Elmah.Gelf.ErrorGelfModule, Elmah.Gelf" preCondition="managedHandler" />
  </modules>
 </system.webServer>
 ...
  <elmah>
    <errorGelf endpoint="udp://127.0.0.1:12201" facility="Gelf" ignoredProperties="ALL_RAW ALL_HTTP" />
  </elmah>
<configuration>
```

### Result
We send alert messages to collect error logs on GrayLog server. 
This allows us to take monitoring, statistic and other usefull analytic functions using Lucene syntax.