# Official image plus the ICU analysis plugin (post 04: icu_folding, icu_collation_keyword).
ARG ES_VERSION=9.5.6
FROM docker.elastic.co/elasticsearch/elasticsearch:${ES_VERSION}
RUN bin/elasticsearch-plugin install --batch analysis-icu
# Post 11: the snapshot directory must exist in the image so the named volume is owned by the elasticsearch user.
RUN mkdir -p /usr/share/elasticsearch/snapshots
