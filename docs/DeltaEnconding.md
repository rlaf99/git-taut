
## Delta Encoding

The delta-encoding uses a simple strategy. 
If a commit has a single parent, *git-taut* performs a diff between the parent and the commit, and decides for each file in the diff whether to store just the delta or the whole content.
If the size of the delta is less than a certain percentage (roughly 60%) of the size of the whole content, then the delta is stored, otherwise the whole content is stored.

The delta is in git-diff format, similar to the output seen from `git diff`, but only contains a single-file's worth.

Delta-encoding does not apply to files in a commit that has zero or more than one parent.