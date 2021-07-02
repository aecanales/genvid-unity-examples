# 02 - Web View Interaction
Example of how to add interaction to the web view based on information sent from Unity. The sample displays a map and sends a data stream with the bounding box corresponding to each location. The web view user can click on one of these locations to see detailed information.

## Unity Explanation
* **SampleScene:** Scene containing a map sprite and names created via the TextMeshPro component. Each name has a `InteractionSpot.cs` component and a box collider defining the interaction area. The "GenvidSessionManager" prefab has also been added and correctly set-up. The "GenvidStreams" child object has been configured to send a data stream named "spot" as defined in the `InteractionSpotStream.cs` component.
* **InteractionSpot.cs:** Contains the detailed information related to this location. Also creates the `BoundingBox` struct by transforming world space into screen space with Unity camera methods. 
* **InteractionSpotStream.cs:** Script that handles sending the data stream. Note how we build a more complicated struct composed by an array of `BoundingBox` structs.

## Web View Explanation
_Coming soon_

## Other Notes
Given that the data never changes, it is a bit of overkill to send the bounding boxes in a data stream. That said, if we were to move the camera, the bounding boxes would change, and it'd make sense to resend the bounding box data.