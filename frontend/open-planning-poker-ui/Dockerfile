FROM node:18 AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm install
COPY . .
RUN npm run build --prod

FROM nginx:alpine
COPY --from=build /app/dist/opp-planning-poker-ui /usr/share/nginx/html

EXPOSE 80

# Start Nginx to serve the app
# daemon off because of docker container, it is easier to monitor
CMD ["nginx", "-g", "daemon off;"]
