# Open Rails Triage

A command-line tool for triage of Open Rail's various roadmap and bug systems.

## Commit triage

* All commits must match one of:
  * Link to Launchpad bug or "bug _number_" or "bug #_number_" to indicate a bug fix
  * Link to Launchpad blueprint to indicate a feature update
  * The pattern "Revert "_commit message_"" to revert an earlier commit
  * The word "manual" to indicate documentation changes
  * The word "locale" or "locales" to indicate localisation changes
  * The word "website" to indicate website changes

## Bug triage

* "Ideal title: _title_" - for crash bugs with a log included, the suggested bug summary is "_exception_ at _location_ (_version_, _route_, _activity_)".
* "Missing known tag _tag_" and "Extra known tag _tag_" - for crash bugs with a log included, the suggested set of tags is:
  * **ai** when the log contains:
    * Exception at ORTS.AI
    * Exception at Orts.Simulation.AIs.
  * **content** when the log contains:
    * MSTS.Parsers.STFException at
    * Orts.Parsers.Msts.STFException at
    * System.IO.InvalidDataException at ORTS.
    * System.IO.InvalidDataException at Orts.
    * Exception at MSTS.
    * Exception at Orts.Formats.
    * Exception at Orts.Parsers.
  * **crash** always
  * **graphics** when the log contains:
    * Exception at MSTS.ACEFile.
    * Exception at ORTS.SuperElevation
    * Exception at ORTS.Viewer3D.
    * Exception at Orts.Viewer3D.
    * _Exceptions:_
      * **cameras** instead of **graphics** when the log contains:
        * Camera
      * **sounds** instead of **graphics** when the log contains:
        * OpenAL
        * Sound
        * WaveFile
      * Nothing instead of **graphics** when the log contains:
        * Processes
  * **menu** when the log contains:
    * Exception at ORTS.Menu.
  * **multiplayer** when the log contains:
    * Exception at ORTS.MultiPlayer.
    * Exception at Orts.MultiPlayer.
  * **physics** when the log contains:
    * Exception at Orts.Simulation.Physics.
    * Exception at Orts.Simulation.RollingStocks.
  * **signals** when the log contains:
    * Exception at ORTS.Signal
    * Exception at ORTS.Deadlock
    * Exception at ORTS.TrackCircuit
    * Exception at ORTS.Signals.
    * Exception at Orts.Simulation.Signalling.
  * **timetable** when the log contains:
    * Exception at Orts.Simulation.Timetables.
  * Any of the following tags on the bug, not matching above, are suggested to be removed:
    * ai
    * cameras
    * content
    * crash
    * graphics
    * menu
    * multiplayer
    * physics
    * signals
    * sounds
    * timetable
* "Status should be Triaged" when:
  * Current status is one of:
    * New
    * Confirmed
  * Tags are set:
    * crash
  * Tags are not set:
    * content
* "Status should be Invalid" when:
  * All these tags are set:
    * crash
    * content
* "Code was committed but bug is not in progress or fixed"
* "Code was committed exclusively more than 28 days ago but bug is not fixed"
* "No code was committed but bug is fixed"
* "No assignee set but bug is in progress"
* "No assignee set but bug is fixed"
* "No milestone set but bug is fixed"

## Blueprint triage

* "Direction is approved but priority is missing"
* "Definition is approved but direction is not approved"
* "Definition is approved but no _link type_ link is found" - when a discussion link and/or roadmap link is missing
* "Definition is approved not no normal _link type_ link is found" - when a discussion link and/or roadmap link is present but not in the expected form
* "Definition is approved but approver is missing"
* "Definition is drafting (or later) but drafter is missing"
* "Implementation is started (or later) but definition is not approved"
* "Implementation is started (or later) but assignee is missing"
* "Implementation is completed but milestone is missing"
* "Code was committed but milestone is _blueprint milestone_ (expected missing/_current milestone_)"
* "Code was committed but definition is not approved"
* "Code was committed exclusively more than 28 days ago but implementation is not complete"
* "No code was committed but implementation for current milestone is complete"

## Roadmap triage

* "(card): has more votes than card above"
* "(card): no _link type_ link is found" - when a discussion link and/or blueprint link is missing
* "(card): no normal _link type_ link is found" - when a discussion link and/or blueprint link is present but not in the expected form
* "(card): no labels found" - when required labels are missing
* "(card): no _type_ checklist found" - when a required checklist is missing
* "(card): _type_ checklist order is _actual order_; expected _expected order_ - when checklist items are in the wrong order
* "(card): _type_ checklist item _name_ is _complete_; expected _complete_
