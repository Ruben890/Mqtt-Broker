
# MQTT Broker with TLS via Nginx

This README provides the configuration for an internal MQTT broker and a secure MQTTS proxy using Nginx with Let's Encrypt TLS certificates.

## Contents

* **Internal MQTT Broker** (listening on localhost)
* **Secure MQTTS Proxy** with Nginx (TLS termination)
* Real-time logs and monitoring
* Firewall and port-level security

---

# Requirements

* Ubuntu 22.04 or later
* Nginx with the `stream` module (`nginx-extras`)
* TLS certificates (Let's Encrypt)
* UFW enabled and configured

---

## Nginx MQTTS Configuration

### Install `nginx-extras`

```bash
sudo apt update
sudo apt install nginx-extras
```

### `stream` block for MQTT/TLS

File: `/etc/nginx/conf.d/mqtt.conf`

```nginx
stream {
    log_format mqtt_log '$remote_addr [$time_local] '
                       '$protocol $status $bytes_sent $bytes_received '
                       '$session_time';

    upstream mqtt_backend {
        server 127.0.0.1:8083;
    }

    server {
        listen 8084 ssl;
        proxy_pass mqtt_backend;

        ssl_certificate     /etc/letsencrypt/live/mqtt.com/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/mqtt.com/privkey.pem;

        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        access_log /var/log/nginx/mqtt_access.log mqtt_log;
        error_log  /var/log/nginx/mqtt_error.log warn;
    }
}
```

* External clients use **8084** with TLS.
* The internal broker listens on **8083** without TLS.

---

### Firewall

```bash
sudo ufw allow 8084/tcp
sudo ufw status
```

* Keep port **8083** accessible only from localhost.

---

## Real-Time Monitoring

* TCP connections:

```bash
watch -n 2 "sudo ss -tulpn | grep -E '8083|8084'"
```

* Nginx logs:

```bash
sudo tail -f /var/log/nginx/mqtt_access.log
sudo tail -f /var/log/nginx/mqtt_error.log
```

* Internal MQTT traffic (non-TLS):

```bash
sudo tcpdump -i any port 8083 -vv
```

---

## Final Flow

```
External MQTT Client (TLS)
   |
   | TLS
   v
Nginx MQTTS Proxy (8084)
   |
   | TCP
   v
Internal MQTT Broker (127.0.0.1:8083)
```

* Debug: connect directly to port **8083** (no TLS)
* Production: connect to **8084** with TLS through Nginx

---

With this setup:

* TLS termination is handled by Nginx
* The internal MQTT broker does **not** expose TLS
* Dedicated logs for MQTT through Nginx
* Firewall-restricted architecture

---
