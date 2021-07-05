// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
import * as express from "express";
import * as basicAuth from "basic-auth";

let admins = {
    "admin": { password: "admin" },
    "user1": { password: "user1" },
    "user2": { password: "user2" },
};

export function authenticate(req: express.Request, res: express.Response, next: express.NextFunction) {
    let user = basicAuth(req);
    if (!user || !admins[user.name] || admins[user.name].password !== user.pass) {
        res.set("WWW-Authenticate", "Basic realm='example'");
        return res.status(401).send();
    }
    return next();
};
