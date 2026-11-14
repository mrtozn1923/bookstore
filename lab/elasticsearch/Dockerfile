# Official image plus the ICU analysis plugin (post 04: icu_folding, icu_collation_keyword).
ARG ES_VERSION=9.5.6
FROM docker.elastic.co/elasticsearch/elasticsearch:${ES_VERSION}
RUN bin/elasticsearch-plugin install --batch analysis-icu
