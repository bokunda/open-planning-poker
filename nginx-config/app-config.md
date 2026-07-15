# nginx setup

## Create config file

`sudo nano /etc/nginx/sites-available/app.openplanningpoker.com`

### Config
```
server {
    listen 80;
    server_name app.openplanningpoker.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name app.openplanningpoker.com;

    ssl_certificate /etc/letsencrypt/live/app.openplanningpoker.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/app.openplanningpoker.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header X-Robots-Tag "index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1" always;

    # Gzip
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/json image/svg+xml font/woff2 font/woff;

    # Sitemap — proxy directly to GameEngine
    location = /sitemap.xml {
        proxy_pass http://127.0.0.1:9091;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
    }

    # Reverse proxy for HTTP requests
    location / {
        proxy_pass http://127.0.0.1:10000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # WebSocket support for GraphQL subscriptions
    location /graphql {
        proxy_pass http://127.0.0.1:10000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "Upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

>  **Important**: Use `127.0.0.1` for proxy_pass (localhost), not the public IP. The `location ~*` static asset cache block was removed — without `proxy_pass` inside it, assets 404.
```

### Create link

`sudo ln -s /etc/nginx/sites-available/app.openplanningpoker.com /etc/nginx/sites-enabled/`

### Create certificate

`sudo certbot --nginx -d app.openplanningpoker.com`

### Test config

`sudo nginx -t`

### Restart nginx

`sudo systemctl restart nginx`
