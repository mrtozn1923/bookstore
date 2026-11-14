# syntax=docker/dockerfile:1
# Two broken entrypoint scripts: no shebang line (job-noshebang.sh) and Windows line endings (job-crlf.sh)
FROM alpine:3
ARG SCRIPT=job-noshebang.sh
COPY --chmod=755 ${SCRIPT} /job.sh
ENTRYPOINT ["/job.sh"]
