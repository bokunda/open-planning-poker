# nginx setup

## Create config file

`sudo nano /etc/nginx/sites-available/apigateway.openplanningpoker.com`

### Config
```
server {
    listen 80;
    server_name apigateway.openplanningpoker.com;

    location / {
        return 301 https://$host$request_uri;
    }
}

server {
    server_name apigateway.openplanningpoker.com;

    location / {
        proxy_pass http://localhost:11000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    listen 443 ssl; # managed by Certbot
    ssl_certificate /etc/letsencrypt/live/apigateway.openplanningpoker.com/fullchain.pem; # managed by Certbot
    ssl_certificate_key /etc/letsencrypt/live/apigateway.openplanningpoker.com/privkey.pem; # managed by Certbot
    include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot

}
server {
    if ($host = apigateway.openplanningpoker.com) {
        return 301 https://$host$request_uri;
    } # managed by Certbot


    listen 80;
    server_name apigateway.openplanningpoker.com;
    return 404; # managed by Certbot
}
```

### Create link

`sudo ln -s /etc/nginx/sites-available/apigateway.openplanningpoker.com /etc/nginx/sites-enabled/`

### Create certificate

`sudo certbot --nginx -d apigateway.openplanningpoker.com`

### Test config

`sudo nginx -t`

### Restart nginx

`sudo systemctl restart nginx`
