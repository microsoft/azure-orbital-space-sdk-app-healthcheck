#!/bin/bash
#
#  Contains the post build steps used by this repo for python debug and wheel generation
#  This is called by the csproj file and should not be called directly
#
# arguments:
#
#   debug - Debug configuration requested
#   release - Release configuration requested
#   target-dir - Directory to place the files
#
# Example Usage:
#
#  "bash ./vscode/postBuild [--debug] [--release] --target-dir ./tmp/someDirectory"


# If we're running in the devcontainer with the k3s-on-host feature, source the .env file
[[ -f "/devfeature/k3s-on-host/.env" ]] && source /devfeature/k3s-on-host/.env

# Pull in the app.env file built by the feature
[[ -n "${SPACEFX_DEV_ENV}" ]] && [[ -f "${SPACEFX_DEV_ENV}" ]] && source "${SPACEFX_DEV_ENV:?}"


#-------------------------------------------------------------------------------------------------------------
source "${SPACEFX_DIR:?}/modules/load_modules.sh" $@ --log_dir "${SPACEFX_DIR:?}/logs/${APP_NAME:?}"

cd ${CONTAINER_WORKING_DIR}

############################################################
# Script variables
############################################################
DEBUG=false
RELEASE=false
TARGET_DIR=""

############################################################
# Help                                                     #
############################################################
function show_help() {
   # Display Help
   echo "Contains the post build steps used by this repo for python debug and wheel generation."
   echo
   echo "Syntax: bash ./vscode/postBuild [--debug] [--release] --output_dir ./tmp/someDirectory"
   echo "options:"
   echo "--debug                            [OPTIONAL] Debug configuration requested"
   echo "--release                          [OPTIONAL] Release configuration requested"
   echo "--target-dir                       [OPTIONAL] Directory to place the files"
   echo "--help | -h                        [OPTIONAL] Help script (this screen)"
   echo
   exit 1
}



############################################################
# Process the input options. Add options as needed.        #
############################################################
# Get the options

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -h|--help) show_help ;;
        --target-dir)
            shift
            TARGET_DIR=$1
            if [[ ! ${TARGET_DIR:0:1} == "/" ]]; then
                TARGET_DIR="${CONTAINER_WORKING_DIR}/dotnet-src/${TARGET_DIR}"
            fi

            # Remove trailing slash if there is one
            OUTPUT_DIR=${OUTPUT_DIR%/}
            ;;
        --debug)
            DEBUG=true
            ;;
        --release)
            RELEASE=true
            ;;
        *) echo "Unknown parameter passed: $1"; show_help ;;
    esac
    shift
done

############################################################
# Copy sampleData to output directory
############################################################
function copy_sampledata_dir() {
    info_log "START: ${FUNCNAME[0]}"

    info_log "Copying '${CONTAINER_WORKING_DIR}/sampleData' to '${TARGET_DIR}/sampleData'..."
    run_a_script "cp -rf ${CONTAINER_WORKING_DIR}/sampleData ${TARGET_DIR}/sampleData"

    info_log "...successfully copied '${CONTAINER_WORKING_DIR}/sampleData' to '${TARGET_DIR}/sampleData'..."

    info_log "END: ${FUNCNAME[0]}"
}


function main() {
    write_parameter_to_log DEBUG
    write_parameter_to_log RELEASE
    write_parameter_to_log TARGET_DIR
    write_parameter_to_log PWD

    copy_sampledata_dir

    info_log "------------------------------------------"
    info_log "END: ${SCRIPT_NAME}"
}

main