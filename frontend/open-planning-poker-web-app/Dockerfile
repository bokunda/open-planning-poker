FROM node:23 AS build-stage

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

RUN npm run build --omit=dev
RUN ls -alh /app/dist
RUN find /app/dist

FROM nginx:alpine AS production-stage

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build-stage /app/dist/open-planning-poker-ui/* /usr/share/nginx/html

EXPOSE 80

# Start NGINX to serve the app
CMD ["nginx", "-g", "daemon off;"]
