# Self-hosted Azure Pipelines agent for Bookstore builds. One container runs exactly one job (see start.sh).
FROM ubuntu:24.04

ENV TARGETARCH="linux-x64"

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl git jq libicu74 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /azp
COPY ./start.sh ./
RUN chmod +x ./start.sh \
    && useradd -m -d /home/agent agent \
    && chown -R agent:agent /azp /home/agent

# The agent runs as a non-root user without sudo.
USER agent
ENTRYPOINT ["./start.sh"]
