FROM alpine:3.4
RUN apk add --no-cache nodejs \
    && adduser -S node \
    && mkdir -p /usr/src/app
WORKDIR /usr/src/app
COPY . /usr/src/app
RUN chown -R node /usr/src/app
USER node
RUN npm install --production
ENTRYPOINT ["npm"]
CMD ["start"]
EXPOSE 3000
