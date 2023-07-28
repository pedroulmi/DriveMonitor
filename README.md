#Drive Monitor
## Core overview
### Singletons:
- DiskManager
- FolderManager
  - Responsible for storing and managing Folder references
- WatcherManager
  - Responsible for storign and managing FileSystemWatcher references

###Threading:
- Manager
  - A abstract class that describes the base implementation of a threaded process to handle a list of jobs and the jobs derived from it.
- JobManager
  - An implementation of the manager abstract class to handle discovery & analyze jobs
- IJob
  - A interface that describes the api for jobs that are used on the manager
- AnalyzeJob
  - A implementation of a job that analyzes a given folder for it's content
- ExploreJob
  - A implementation of a job that explores a given folder and generates the ExploreJobs for its subdirectories and a AnalyzeJob for itself.
- Pooler
  - A unused class that describes the base implementation of a thread that repeats a list of actions every N interval.

###Data Objects
- Folder
  - Describes a Folder in the system

###Notifiables
- IFolderDiscoveryNotifiable
  - Interface that describes the api of a notifiable object that tracks folder discoveries events
- IFolderNotifiable
  - Interface that describes the api of a notifiable object taht tracks changes on folders