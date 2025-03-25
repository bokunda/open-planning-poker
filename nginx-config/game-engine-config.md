# nginx setup

## Create config file

`sudo nano /etc/nginx/sites-available/ge.openplanningpoker.com`

### Config
```
server {
    listen 80;
    server_name ge.openplanningpoker.com;

    location / {
        return 301 https://$host$request_uri;
    }
}

server {
    listen 443 ssl;
    server_name ge.openplanningpoker.com;

    ssl_certificate /etc/letsencrypt/live/ge.openplanningpoker.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/ge.openplanningpoker.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Reverse proxy for HTTP requests
    location / {
        proxy_pass http://162.55.213.9:9091;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # WebSocket support for GraphQL subscriptions
    location /graphql {
        proxy_pass http://162.55.213.9:9091;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "Upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

### Create link

`sudo ln -s /etc/nginx/sites-available/ge.openplanningpoker.com /etc/nginx/sites-enabled/`

### Create certificate

`sudo certbot --nginx -d ge.openplanningpoker.com`

### Test config

`sudo nginx -t`

### Restart nginx

`sudo systemctl restart nginx`
