# About
Event Store Browser is a simple, quick'n'dirty tool to explore and interact with event streams in an Event Store instance.

# Features
* Explore the events in an event stream (with nicely indented JSON)
* Copy a list of events and paste them into your favorite text editor for easy searching
* Copy events from one stream to another
* Move the beginning of a stream (the "point of truncation") to simulate soft-deletes
* Revert to previous soft-delete

# Warning
Be careful when using this tool!
It has not been thoroughly tested and should only be used for debugging purposes on your local (non-production) databases!
