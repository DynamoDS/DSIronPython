# DSIronPython

Contains the DSIronPython package for using the legacy IronPython2 engine in Dynamo.
Building this soluton will produce a dynamo package containing the python engine and an extension to load it.
This repo is a WIP.

### How to publish a new version
We currently can't use the release PR option when making releases on mirrored repositories. There is a propsed follow-up improvment filed that would allow CILibrary to create a PR against the public repository instead (DYN-8724). But until that is done, our release process will be:

- Create a release branch on the internal repository
- Build the branch to publish
- Manually create a PR on the public repository with the changes introduced by the release branch (the updated version number in the pipeline file). 
- Review and merge this PR
- Delete the release branch on the internal repository