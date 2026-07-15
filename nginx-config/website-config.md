# nginx setup

## Create config file

`sudo nano /etc/nginx/sites-available/openplanningpoker.com`

### Config
```
# www → non-www redirect
server {
    listen 80;
    server_name www.openplanningpoker.com;
    return 301 https://openplanningpoker.com$request_uri;
}

server {
    listen 80;
    server_name openplanningpoker.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name www.openplanningpoker.com;
    ssl_certificate /etc/letsencrypt/live/openplanningpoker.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/openplanningpoker.com/privkey.pem;
    return 301 https://openplanningpoker.com$request_uri;
}

server {
    listen 443 ssl http2;
    server_name openplanningpoker.com;

    ssl_certificate /etc/letsencrypt/live/openplanningpoker.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/openplanningpoker.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header X-Robots-Tag "index, follow, max-image-preview:large, max-snippet:-1, max-video-preview:-1" always;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/json image/svg+xml font/woff2 font/woff;

    # Cache static assets (1 year)
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff2|woff)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Reverse proxy
    location / {
        proxy_pass http://162.55.213.9:9010;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Custom 404
    error_page 404 /404.html;
}
```

### Create link

`sudo ln -s /etc/nginx/sites-available/openplanningpoker.com /etc/nginx/sites-enabled/`

### Create certificate

`sudo certbot --nginx -d openplanningpoker.com`

### Test config

`sudo nginx -t`

### Restart nginx

`sudo systemctl restart nginx`
