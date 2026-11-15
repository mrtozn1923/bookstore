{{/*
Chart name, overridable with nameOverride.
*/}}
{{- define "bookstore.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Release-scoped base name. Resource names add a component suffix: <fullname>-api, <fullname>-web.
If the release name already contains the chart name, it is used as is.
*/}}
{{- define "bookstore.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Selector labels for one component. Usage: include "bookstore.selectorLabels" (dict "ctx" . "component" "api")
These never change during the life of a release: Deployment selectors are immutable.
*/}}
{{- define "bookstore.selectorLabels" -}}
app.kubernetes.io/name: {{ include "bookstore.name" .ctx }}
app.kubernetes.io/instance: {{ .ctx.Release.Name }}
app.kubernetes.io/component: {{ .component }}
{{- end }}

{{/*
All labels for one component: selector labels plus version and chart information.
*/}}
{{- define "bookstore.labels" -}}
{{ include "bookstore.selectorLabels" . }}
app.kubernetes.io/version: {{ .ctx.Chart.AppVersion | quote }}
app.kubernetes.io/part-of: bookstore
app.kubernetes.io/managed-by: {{ .ctx.Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .ctx.Chart.Name .ctx.Chart.Version | replace "+" "_" | trunc 63 }}
{{- end }}

{{/*
Image reference. Usage: include "bookstore.image" (dict "ctx" . "image" .Values.web.image)
An empty tag falls back to appVersion from Chart.yaml.
*/}}
{{- define "bookstore.image" -}}
{{- $repository := required "image.repository is required" .image.repository -}}
{{- printf "%s:%s" $repository (.image.tag | default .ctx.Chart.AppVersion) -}}
{{- end }}

{{/*
Pod and container security settings shared by every component (see the Kubernetes series, post 15).
*/}}
{{- define "bookstore.podSecurityContext" -}}
runAsNonRoot: true
seccompProfile:
  type: RuntimeDefault
{{- end }}

{{- define "bookstore.containerSecurityContext" -}}
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true
capabilities:
  drop: ["ALL"]
{{- end }}
