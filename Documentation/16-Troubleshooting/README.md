# Troubleshooting Guide

## Purpose

This guide provides solutions for common issues that may occur in the School Management System.

## Login Issues

### Cannot Log In

**Symptoms**: Login page does not accept credentials

**Possible Causes and Solutions:**

1. **Incorrect credentials**
   - Verify username/email is correct
   - Check Caps Lock is off
   - Use **Forgot Password** to reset

2. **Account locked**
   - After 5 failed attempts, account is locked for 15 minutes
   - Contact administrator to unlock

3. **Account disabled**
   - Contact system administrator
   - Administrator can re-enable the account

4. **Session expired**
   - Clear browser cookies and cache
   - Try incognito/private browsing mode

5. **JWT token expired**
   - Log out and log in again
   - Tokens are automatically refreshed

### Cannot Register

**Symptoms**: Registration form does not submit

**Solutions:**
- Check all required fields are filled
- Password must meet complexity requirements (12+ chars, uppercase, lowercase, digit, special character)
- Email must not already be registered
- Verify registration is enabled by the institution

## Permission Issues

### Access Denied

**Symptoms**: See "Access Denied" or "Forbidden" messages

**Solutions:**
- Verify your role has the required permissions
- Contact administrator to grant necessary role
- Log out and log in again to refresh permissions

### Cannot Access Admin Functions

**Symptoms**: Admin menu items not visible

**Solutions:**
- Verify you have the Administrator role
- Check if you are logged in with the correct account
- Contact another administrator for role assignment

## Docker Issues

### Container Won't Start

**Symptoms**: Docker containers fail to start

**Troubleshooting Steps:**

```bash
# Check container status
docker compose ps

# View logs for specific container
docker compose logs api
docker compose logs postgres

# Check port conflicts
netstat -ano | findstr :5432
netstat -ano | findstr :8080

# Restart containers
docker compose down
docker compose up -d
```

### Port Already in Use

**Symptoms**: Error about port already allocated

**Solutions:**
```bash
# Change port in docker-compose.yml (file name) or .env
# Example: change 8080:8080 to 8081:8080
```

### PostgreSQL Connection Refused

**Symptoms**: API cannot connect to database

**Solutions:**
```bash
# Check PostgreSQL is running
docker compose ps postgres

# Check PostgreSQL logs
docker compose logs postgres

# Verify connection string in .env
# Restart PostgreSQL
docker compose restart postgres
```

## Database Issues

### Migration Failed

**Symptoms**: Error during database migration

**Solutions:**
```bash
# Check migration status (requires EF Core tools installed)
docker compose exec api dotnet ef migrations list --project src/SMS.Persistence

# Re-run migrations
docker compose exec api dotnet SMS.API.dll migrate-database

# Rollback last migration (requires EF Core tools installed)
docker compose exec api dotnet ef migrations remove --project src/SMS.Persistence
```

### Database Connection Lost

**Symptoms**: Health check fails, queries time out

**Solutions:**
1. Check PostgreSQL container is running
2. Verify network connectivity
3. Check connection pool exhaustion
4. Restart PostgreSQL if necessary
5. Increase connection pool size if needed

### Slow Queries

**Symptoms**: Pages load slowly, API responses delayed

**Solutions:**
- Run ANALYZE to update statistics
- Check for missing indexes
- Review query execution plans
- Increase database resources

## Upload Issues

### File Upload Fails

**Symptoms**: Cannot upload files

**Solutions:**
1. **File too large**: Check max file size configuration
2. **Unsupported type**: Verify file extension is allowed
3. **Storage full**: Check disk space
4. **Permission denied**: Check upload directory permissions

```bash
# Check disk space
df -h

# Check upload directory exists
ls -la uploads/

# Check directory permissions
chmod -R 755 uploads/
```

### File Download Fails

**Symptoms**: Cannot download uploaded files

**Solutions:**
1. Verify file exists in storage
2. Check file path in database
3. Verify Nginx static file serving is configured
4. Check file permissions

## Authentication Issues

### JWT Token Invalid

**Symptoms**: API returns 401 Unauthorized

**Solutions:**
1. Clear browser cookies and log in again
2. Verify JWT_SECRET has not changed
3. Check token expiration
4. If JWT_SECRET changed, all users must log in again

### Token Revocation Not Working

**Symptoms**: Logged out users can still access API

**Solutions:**
1. Verify Redis is running (if using Redis revocation)
2. Check InMemoryTokenRevocation in development
3. Restart API to clear in-memory cache

## Performance Issues

### Slow API Responses

**Symptoms**: API takes long to respond

**Solutions:**
1. Check database query performance
2. Monitor memory and CPU usage
3. Review slow endpoint metrics
4. Scale API instances if needed
5. Enable response caching

### High Memory Usage

**Symptoms**: Application consumes excessive memory

**Solutions:**
1. Restart the API container
2. Check for memory leaks
3. Increase container memory limits
4. Monitor with `docker stats`

## Deployment Issues

### Deployment Fails

**Symptoms**: New version fails to deploy

**Solutions:**
1. Check build logs for errors
2. Verify all dependencies are available
3. Check configuration changes
4. Rollback to previous version
5. Review migration scripts

### SSL Certificate Issues

**Symptoms**: Browser shows security warning

**Solutions:**
1. Check certificate expiry date
2. Verify certificate chain is complete
3. Renew certificate if expired
4. Check Nginx SSL configuration
5. Restart Nginx after certificate update

## Configuration Issues

### Environment Variables Not Applied

**Symptoms**: Configuration changes not taking effect

**Solutions:**
1. Verify variable name is correct
2. Restart the API after changing variables
3. Check for typos in variable names
4. Verify .env file is in the correct location

### CORS Errors

**Symptoms**: Browser shows CORS errors in console

**Solutions:**
1. Verify `Frontend:Url` matches your frontend URL
2. Check CORS configuration in appsettings.json
3. Restart API after CORS changes
4. Check for protocol mismatches (http vs https)

## Recovery Issues

### Restore Fails

**Symptoms**: Database restore fails

**Solutions:**

1. Verify backup file is not corrupted:
   ```bash
   pg_restore -l backup.dump | head -5
   ```
2. Check PostgreSQL version compatibility
3. Ensure sufficient disk space
4. Check for active connections to the database:
   ```bash
   SELECT * FROM pg_stat_activity WHERE datname = 'SchoolManagementSystem';
   ```

### Backup Fails

**Symptoms**: Backup process fails

**Solutions:**

1. Check disk space for backup destination
2. Verify database connectivity
3. Check backup script permissions
4. Test backup manually:
   ```bash
   pg_dump -h localhost -U sms_user -d SchoolManagementSystem -F c -f test_backup.dump
   ```

## Notification Issues

### Notifications Not Delivered

**Symptoms**: Users not receiving notifications

**Solutions:**

1. Check SignalR hub connection:
   - Browser console should show SignalR connected
   - Verify `/hub` endpoint is accessible
2. Check notification service is registered
3. Verify user is subscribed to correct notification channels
4. Check browser permissions for notifications

### SMS Notifications Not Working

**Symptoms**: SMS notifications not sent

**Solutions:**

1. Verify Twilio credentials are configured
2. Check SMS service is registered
3. Verify phone numbers are formatted correctly
4. Check Twilio account balance
5. Review Twilio logs for delivery status

## Frequently Asked Questions

**Q: How do I reset my password?**
A: Contact your administrator who can generate a password reset token.

**Q: How do I report a bug?**
A: Document the steps to reproduce the issue and contact the system administrator with logs and screenshots.

**Q: How do I request a new feature?**
A: Contact the system administrator with your requirements.

**Q: How do I check system status?**
A: Access the /health endpoint or check the system status dashboard.

**Q: What browsers are supported?**
A: Chrome, Firefox, Edge, and Safari (latest versions).

## Related Documentation

- [System Administration Guide](../06-System-Administration/README.md)
- [Installation Guide](../03-Installation/README.md)
- [Deployment Guide](../04-Deployment/README.md)
- [Maintenance Guide](../15-Maintenance/README.md)
- [Backup and Recovery Guide](../14-Backup-and-Recovery/README.md)
