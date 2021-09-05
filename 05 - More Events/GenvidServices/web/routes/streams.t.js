"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.router = void 0;
// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
const express = require("express");
const rp = require("request-promise");
const config = require("../utils/config");
exports.router = express.Router();
exports.router.get("/streams", (_req, res) => __awaiter(void 0, void 0, void 0, function* () {
    let options = {
        method: "GET",
        baseUrl: config.disco_url,
        uri: "/disco/stream/info",
        headers: {
            secret: config.disco_secret
        },
        json: true
    };
    try {
        let info = yield rp(options);
        res.status(200).send(info);
    }
    catch (err) {
        res.status(500).send(err);
    }
}));
exports.router.post("/channels/join", (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    let body = req.body;
    if (!body) {
        throw new Error("Bad request");
    }
    let joinOptions = {
        method: "POST",
        baseUrl: config.disco_url,
        uri: "/disco/stream/join",
        headers: {
            secret: config.disco_secret
        },
        json: true
    };
    try {
        let data = yield rp(joinOptions);
        res.status(200).send(data);
    }
    catch (err) {
        res.status(500).send(err.error);
    }
}));
//# sourceMappingURL=streams.t.js.map