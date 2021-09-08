# 01 - Data Streams
Basic examples that contains a rotating cube. Demostrates how to set up a scene and send data about the cube to a Genvid stream via data streams, to then display them in web view.

![Data Stream example](../img/01.gif)

## Important Files
* **SampleScene:** Basic scene that contains a Unity primitive cube with the `Cube.cs` component. The "GenvidSessionManager" prefab has also been added and correctly set-up. The "GenvidStreams" child object has been configured to send a data stream named "Cube" as defined in the `DataStream.cs` component.
* **Cube.cs:** Basic script that rotates the cube and changes it's color.
* **DataStream.cs:** Script that handles sending the data (in this case, the cube's color components and rotation in euler angles). 

## Relevant Web View Code
We load the data from the stream and apply it to HTML elements.
```typescript
private on_new_frame(frameSource: genvid.IDataFrame) {
    let cubeData = JSON.parse(frameSource.streams.Cube.data);

    let rotationX: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultMine");
    let rotationY: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultHealth");
    let rotationZ: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultMovement");
    let colorR: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultAttack");
    let colorG: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultShield");
    let colorB: HTMLElement = <HTMLDivElement>document.querySelector("#colorB");
    
    rotationX.textContent = "Rotation X: " + Math.round(cubeData.CubeRotationX);
    rotationY.textContent = "Rotation Y: " + Math.round(cubeData.CubeRotationY);
    rotationZ.textContent = "Rotation Z: " + Math.round(cubeData.CubeRotationZ);
    colorR.textContent = "Color R: " + Math.round(cubeData.CubeColorR * 10) / 10;
    colorG.textContent = "Color G: " + Math.round(cubeData.CubeColorG * 10) / 10;
    colorB.textContent = "Color B: " + Math.round(cubeData.CubeColorB * 10) / 10;
    ...
}
```