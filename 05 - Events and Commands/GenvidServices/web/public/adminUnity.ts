// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
import * as genvid from "genvid";

namespace unityTankSample {

    // command interface used to communicate with the game
    interface ICommandRequest {
        id: string;
        value: string;
    }

    export class AdminController {
        message: string = "";
        error: string = "";

        client: genvid.IGenvidClient;
        streamInfo: genvid.IStreamInfo;

        playerTableSetup: boolean = false;

        // Time when the broadcast started (matches what is present in game data).
        last_game_time_received: number = -1;

        constructor(private video_player_id: string) {

        }

        // Start the connection to the services
        onConnect() {
            let promise = $.post("/api/public/channels/join", {}, (joinRep) => {
                this.on_channel_join(<genvid.IChannelJoinResponse>joinRep);
            });
            promise.fail((err) => {
                alert("Can't get the stream info:" + err);
            });
        }

        // Create the genvid Client and the function listening to it
        private on_channel_join(joinRep: genvid.IChannelJoinResponse) {
            this.streamInfo = joinRep.info;
            this.client = genvid.createGenvidClient(this.streamInfo, joinRep.uri, joinRep.token, this.video_player_id);
            this.client.onStreamsReceived((streams) => { this.on_streams_received(streams); });
            this.client.onDraw((frame) => { this.on_new_frame(frame); });
            this.client.start();
        }

        // Upon receving the stream
        private on_streams_received(dataStreams: genvid.IDataStreams) {
            for (let stream of dataStreams.streams) {
                for (let frame of stream.frames) {
                    if (this.last_game_time_received < frame.timeCode) {
                        this.last_game_time_received = frame.timeCode;
                    }
                }
            }

            // Parse the JSON from each elements
            for (let stream of [...dataStreams.streams, ...dataStreams.annotations]) {
                for (let frame of stream.frames) {
                    frame.user = JSON.parse(frame.data);
                }
            }
        }

        // During a new frame, if the game data is valid, the player table is created
        private on_new_frame(frameSource: genvid.IDataFrame) {
            // Parse the JSON from each elements
            let gameDataFrame = frameSource.streams["GameData"];
            let gameData = null;

            if (gameDataFrame && gameDataFrame.user) {

                if (this.message === "Unable to retreive data from the stream, is it still active ?") {
                    this.message = "Success: data received from the stream";
                    this.displayMessage();
                }
                if (this.playerTableSetup === false) {
                    this.playerTableSetup = true;

                    gameData = gameDataFrame.user;
                    let tanks = gameData.tanks;

                    for (let tank of tanks) {
                        let tankAdd = "<table>" +
                                "<tr>" +
                                    "<td id='table_name'>" + tank.name + "</td>" +
                                "</tr>" +
                                "<tr>" +
                                    "<td id='command_button'><div class='Buff_Tank_" + tank.id + "_Health'>Health Buff</div></td>" +
                                "</tr>" +
                                "<tr>" +
                                    "<td id='command_button'><div class='Buff_Tank_" + tank.id + "_Shield'>Shield Buff</div></td>" +
                                "</tr>" +
                                "<tr>" +
                                    "<td id='command_button'><div class='Buff_Tank_" + tank.id + "_Movement'>Movement Buff</div></td>" +
                                "</tr>" +
                                "<tr>" +
                                    "<td id='command_button'><div class='Buff_Tank_" + tank.id + "_Attack'>Attack Buff</div></td>" +
                                "</tr>" +
                            "</table>";

                        $(".admin_table_section").append(tankAdd);

                        let healthBuffButton = <HTMLButtonElement>document.querySelector(".Buff_Tank_" + tank.id + "_Health");
                        healthBuffButton.addEventListener("click", (_event) => { this.buffTank(tank.name, tank.id, 1); }, false);

                        let attackBuffButton = <HTMLButtonElement>document.querySelector(".Buff_Tank_" + tank.id + "_Attack");
                        attackBuffButton.addEventListener("click", (_event) => { this.buffTank(tank.name, tank.id, 2); }, false);

                        let movementBuffButton = <HTMLButtonElement>document.querySelector(".Buff_Tank_" + tank.id + "_Movement");
                        movementBuffButton.addEventListener("click", (_event) => { this.buffTank(tank.name, tank.id, 3); }, false);

                        let shieldBuffButton = <HTMLButtonElement>document.querySelector(".Buff_Tank_" + tank.id + "_Shield");
                        shieldBuffButton.addEventListener("click", (_event) => { this.buffTank(tank.name, tank.id, 4); }, false);
                    }

                    let matchButtons = "<table>" +
                                "<tr>" +
                                    "<td id='command_button'><div class='restartMatch'>Restart Match</td>" +
                                "</tr>" +
                            "</table>";
                    $(".admin_table_section").append(matchButtons);

                    let restartButton = <HTMLButtonElement>document.querySelector("." + "restartMatch");
                    restartButton.addEventListener("click", (_event) => { this.restartMatch(); }, false);

                }
            }
            else {
                this.message = "Unable to retreive data from the stream, is it still active ?";
                this.displayErrorMessage();
            }
        }

        private getBuffName(id:number): string {
            if (id == 1) return "Health Buff";
            if (id == 2) return "Attack Buff";
            if (id == 3) return "Movement Buff";
            if (id == 4) return "Shield Buff";
        }

        // send buff commands to tanks
        buffTank(tankName: string, tankId: number, buffId: number) {
            this.message = this.error = "";

            let command: ICommandRequest = {
                id: "PowerUpTank",
                value: `${tankId.toString()}:${buffId.toString()}`
            };

            var buffName: string = this.getBuffName(buffId);

            let promise = $.post("/api/admin/commands/game", command).then(() => {
                this.message = `PowerUpTank ${tankName}:${buffName}`;
                this.displayMessage();
            });

            promise.fail((err) => {
                this.message = `Failed with error ${err} to do changeSpeed ${tankName}:${buffName}`;
                this.displayErrorMessage();
            });
        }

        // send restart command
        restartMatch() {
            this.message = this.error = "";

            let command: ICommandRequest = {
                id: "RestartMatch",
                value: "null"
            };

            let promise = $.post("/api/admin/commands/game", command).then(() => {
                this.message = `Restarted Match`;
                this.displayMessage();
            });

            promise.fail((err) => {
                this.message = `Failed with error ${err} to restart the match`;
                this.displayErrorMessage();
            });
        }

        // Display a message in the page as a sucess
        displayMessage() {
                let messageErrorDiv = <HTMLDivElement>document.querySelector("#alert_error_cube");
                messageErrorDiv.style.visibility = "hidden";

                let messageDiv = <HTMLDivElement>document.querySelector("#alert_sucess_cube");
                messageDiv.style.visibility = "visible";
                let messageSpan = <HTMLSpanElement>document.querySelector("#sucess_message_cube");
                messageSpan.textContent = this.message;
        }

        // Display a message in the page as an error
        displayErrorMessage() {
                let messageDiv = <HTMLDivElement>document.querySelector("#alert_sucess_cube");
                messageDiv.style.visibility = "hidden";

                let messageErrorDiv = <HTMLDivElement>document.querySelector("#alert_error_cube");
                messageErrorDiv.style.visibility = "visible";
                let messageSpan = <HTMLSpanElement>document.querySelector("#error_message_cube");
                messageSpan.textContent = this.message;
        }
    }
}

// initialization
let admin = new unityTankSample.AdminController("video_player_hidden");
admin.onConnect();
