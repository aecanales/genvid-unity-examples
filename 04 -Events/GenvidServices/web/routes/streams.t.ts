// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
import * as express from "express";
import * as request from "request";
import * as rp from "request-promise";

import * as config from "../utils/config";

export let router = express.Router();

export interface IChannelJoinRequest {
    channel: string;
    token: string;
}

export interface IChannelJoinResponse {
    info: any;
    uri: string;
    token: string;
}

router.get("/streams", async (_req, res) => {

    let options: request.UriOptions & rp.RequestPromiseOptions = {
        method: "GET",
        baseUrl: config.disco_url,
        uri: "/disco/stream/info",
        headers: {
            secret: config.disco_secret
        },
        json: true
    };

    try {
        let info = await rp(options);
        res.status(200).send(info);
    }
    catch (err) {
        res.status(500).send(err);
    }
});

router.post("/channels/join", async (req, res) => {
    let body: IChannelJoinRequest = req.body;
    if (!body) {
        throw new Error("Bad request");
    }

    let joinOptions: request.UriOptions & rp.RequestPromiseOptions = {
        method: "POST",
        baseUrl: config.disco_url,
        uri: "/disco/stream/join",
        headers: {
            secret: config.disco_secret
        },
        json: true
    };

    try {
        let data: IChannelJoinResponse = await rp(joinOptions);
        res.status(200).send(data);
    }
    catch (err) {
        res.status(500).send(err.error);
    }
});
