// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
import * as express from "express";
import * as streams from "./streams.t";
import * as commands from "./commands";
import * as basicAuth from "../utils/auth";

let router = express.Router();

router.use("/public", streams.router);
router.use("/admin", basicAuth.authenticate, commands.router);

module.exports = router;
