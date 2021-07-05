# 02 - Web View Interaction
Example of how to add interaction to the web view based on information sent from Unity. The sample displays a map and sends a data stream with the bounding box corresponding to each location. The web view user can click on one of these locations to see detailed information.

![Web view interaction example](../img/02.gif)

## Relevant Unity Files
* **SampleScene:** Scene containing a map sprite and names created via the TextMeshPro component. Each name has a `InteractionSpot.cs` component and a box collider defining the interaction area. The "GenvidSessionManager" prefab has also been added and correctly set-up. The "GenvidStreams" child object has been configured to send a data stream named "spot" as defined in the `InteractionSpotStream.cs` component.
* **InteractionSpot.cs:** Contains the detailed information related to this location. Also creates the `BoundingBox` struct by transforming world space into screen space with Unity camera methods. 
* **InteractionSpotStream.cs:** Script that handles sending the data stream. Note how we build a more complicated struct composed by an array of `BoundingBox` structs.

## Relevant Web View Code
We define the following interface to recieve the data stream:
```typescript
export interface BoundingBox {
    X: number;
    Y: number;
    Width: number;
    Height: number;
    Text: string;
}
```

Then, we add these variables to the `UnityController` class. Note that we also added a `<p>` element to the HTML file below the video stream.
```typescript
mapSpots : Array<BoundingBox>;
descriptionText : HTMLParagraphElement;
```

`mapSpots` is updated every frame using the recieved data.
```typescript
private on_new_frame(frameSource: genvid.IDataFrame) {
    this.mapSpots = JSON.parse(frameSource.streams.spot.data).BoundingBoxes;
    ...
}
```

An click event listener is added to the mouse overlay. Whenever the user clicks on the stream, we check whether the click was inside the bounds of one of the map spots and if it is, we change the description accordingly.
```typescript
this.descriptionText = <HTMLParagraphElement> document.querySelector("#description_area");

const isInBounds = (spot, click) => {
    return spot.X <= click.X && click.X <= spot.X + spot.Width &&
           spot.Y <= click.Y && click.Y <= spot.Y + spot.Height;
};

// Returns the click relative to the mouseOverlay div (instead of relative to the entire DOM).
const relativeClickPosition = (click) => {
    const rect = this.mouseOverlay.getBoundingClientRect();
    return {X: click.pageX - rect.x, Y: click.pageY - rect.y}
};

this.mouseOverlay.addEventListener("click", (event) => { 
    if (this.mapSpots) {
        this.mapSpots.forEach(spot => {
            if (isInBounds(spot, relativeClickPosition(event))) {
                this.descriptionText.textContent = spot.Text;
            }
        });
    }
});
```
## Other Notes
Given that the data never changes, it is a bit of overkill to send the bounding boxes in a data stream. That said, if we were to move the camera, the bounding boxes would change and then it'd make sense to resend the bounding box data.