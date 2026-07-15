FROM node:22-alpine AS build-stage

WORKDIR /app

COPY package*.json ./

RUN npm install --legacy-peer-deps

COPY . .

RUN npm run build

FROM node:22-alpine AS production-stage

WORKDIR /app

COPY --from=build-stage /app/dist/open-planning-poker-ui ./dist/open-planning-poker-ui
COPY --from=build-stage /app/package*.json ./

RUN npm install --omit=dev --legacy-peer-deps

EXPOSE 4000

ENV PORT=4000

CMD ["node", "dist/open-planning-poker-ui/server/server.mjs"]
