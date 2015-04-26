Goal:
This project was created in order to host the Resharper annotations. 
Having the Resharper annotations in a custom file rather than using the 
original JetBrains.Annotations namespace allows people to use this 
project even when they do not have Resharper installed.

The Resharper annotations were isolated in a separate project rather than 
as a file in one of the existing projects so that we can exclude it easily from 
code analysis. 

Namespace:
The Resharper annotations are by default in the JetBrains.Annotations namespace. 
This project is intentionally named with a different name and uses a different 
namespace so that it is not causing a namespace collision for developers who 
may want to extend this tool and already use the JetBrains.Annotations namespace.
