# 05 - Events and Commands
Example that shows how to handle player input in a more complex case and how to use commands. Clicking on a bug will destroy it and the game can be restarted by pressing the corresponding button. When asked for credentials, use "admin" without quotes for both the username and password.

Events are optimised for **input that will be sent by many players** such as votes or clicks. This works thanks to a map-reduce algorithm that groups similar inputs and thus reduces the strain on the server hosting the game. On the other hand, commands are not optimized and thus are best reserved for **input only a few users will be able to access** such as administrator controls.

![Events and Commands example](../img/05.gif)

## Relevant Configuration Files
* **GenvidServices/config/events.json:** Contains the definition of event we send from the web view to the game.

## Relevant Unity Files
* **SampleScene:** Scene with a simple background and multiple `Bug` game objects. The "GenvidSessionManager" prefab has  been added and correctly set-up. The child objects "Genvid Streams", "GenvidEvents" and "GenvidCommands" have been configured with the corresponding actions.
* **Bug.cs:** Script that moves a bug back and forth and returns data about the bug's bounding box.
* **BugCommand.cs:** Handles the game restart command when recieved.
* **BugEvent.cs:** Handles deactivating a bug when the click event is recieved.
* **BugStream.cs:** Handles sending the data stream with the bounding box of each bug.

## Relevant Web View Code
We use the same code was the web view interaction example to process input and detect whether a player clicked on a bug. Note how sending an event is done via the ` this.client.sendEventObject({'click': bug.ID})` method:
```typescript
export interface BoundingBox {
    X: number;
    Y: number;
    Width: number;
    Height: number;
    ID: bumber;
}
...
bugs : Array<BoundingBox>;
...
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
                this.client.sendEventObject({'click': bug.ID});
            }
        });
    }
});

private on_new_frame(frameSource: genvid.IDataFrame) {
    this.bugs = JSON.parse(frameSource.streams.bugs.data).BoundingBoxes;
    ...
}
```
To send a command, we add a button to `index.html` and the following code:
```typescript
let restartButton = <HTMLButtonElement>document.querySelector("#restart_game_button");
restartButton.addEventListener("click", (_event) => { 
    let command: ICommandRequest = {
        id: "RestartMatch",
        value: "null"
    }; 
    
    let promise = $.post("/api/admin/commands/game", command).then(() => {
        console.log("Game has been restarted.");
    });

    promise.fail((err) => {
        console.error(`Failed to send command: ${err}`)
    });

}, false);
```            