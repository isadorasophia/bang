## Release 0.0.3

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
BANG0001 | Usage    | Error    | Classes cannot be components.             
BANG0002 | Usage    | Error    | Components must be declared as readonly.  
BANG1001 | Usage    | Error    | System requires FilterAttribute.          
BANG1002 | Usage    | Error    | System requires MessagerAttribute.        
BANG1003 | Usage    | Error    | System requires WatchAttribute.           
BANG1004 | Usage    | Warning  | System does not use MessagerAttribute.    
BANG1005 | Usage    | Warning  | System does not use WatchAttribute.       
BANG2001 | Usage    | Error    | Filter attribute expects only components. 
BANG2002 | Usage    | Error    | Messager attribute expects only messages. 
BANG2003 | Usage    | Error    | Watch attribute expects only components.  
BANG3001 | Usage    | Error    | Classes cannot be messages.               
BANG3002 | Usage    | Error    | Messages must be declared as readonly.    