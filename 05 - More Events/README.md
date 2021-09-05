# 01 - Data Streams
Basic examples that contains a rotating cube. Demostrates how to set up a scene and send data about the cube to a Genvid stream via data streams, to then display them in web view.

![Data Stream example](../img/01.gif)

## Important Files
* **SampleScene:** Basic scene that contains a Unity primitive cube with the `Cube.cs` component. The "GenvidSessionManager" prefab has also been added and correctly set-up. The "GenvidStreams" child object has been configured to send a data stream named "Cube" as defined in the `DataStream.cs` component.
* **Cube.cs:** Basic script that rotates the cube and changes it's color.
* **DataStream.cs:** Script that handles sending the data (in this case, the cube's color components and rotation in euler angles). 

*Description of website files coming soon...*